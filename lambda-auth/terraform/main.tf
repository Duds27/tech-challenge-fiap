locals {
  name = "${var.project}-${var.environment}"
}

# ───────────── Estado remoto dos demais repositórios de infra ─────────────
# Repo 2 (infra-k8s): VPC, subnets privadas, SG dos nós e o NLB interno do app.
data "terraform_remote_state" "k8s" {
  backend = "s3"
  config = {
    bucket = var.state_bucket
    key    = "infra-k8s/${var.environment}/terraform.tfstate"
    region = var.aws_region
  }
}

# Repo 3 (infra-database): endpoint do RDS e ARN do secret de credenciais.
data "terraform_remote_state" "database" {
  backend = "s3"
  config = {
    bucket = var.state_bucket
    key    = "infra-database/${var.environment}/terraform.tfstate"
    region = var.aws_region
  }
}

# ───────────────────────────── IAM da Lambda ─────────────────────────────
data "aws_iam_policy_document" "assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda" {
  name               = "${local.name}-auth-lambda"
  assume_role_policy = data.aws_iam_policy_document.assume.json
}

# Execução básica + ENI na VPC (necessário para acessar o RDS em subnet privada).
resource "aws_iam_role_policy_attachment" "vpc" {
  role       = aws_iam_role.lambda.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole"
}

data "aws_iam_policy_document" "secrets" {
  statement {
    actions   = ["secretsmanager:GetSecretValue"]
    resources = [var.jwt_secret_id, data.terraform_remote_state.database.outputs.db_secret_arn]
  }
}

resource "aws_iam_role_policy" "secrets" {
  name   = "${local.name}-auth-secrets"
  role   = aws_iam_role.lambda.id
  policy = data.aws_iam_policy_document.secrets.json
}

# ─────────────────────── Security group da Lambda ───────────────────────
resource "aws_security_group" "lambda" {
  name        = "${local.name}-auth-lambda-sg"
  description = "SG da Lambda de autenticacao"
  vpc_id      = data.terraform_remote_state.k8s.outputs.vpc_id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# ─────────────────────────── Funções Lambda ───────────────────────────
resource "aws_lambda_function" "auth" {
  function_name    = "${local.name}-auth"
  role             = aws_iam_role.lambda.arn
  runtime          = "nodejs20.x"
  handler          = "handler.handler"
  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)
  timeout          = 15
  memory_size      = 256

  vpc_config {
    subnet_ids         = data.terraform_remote_state.k8s.outputs.private_subnet_ids
    security_group_ids = [aws_security_group.lambda.id]
  }

  environment {
    variables = {
      JWT_SECRET_ID       = var.jwt_secret_id
      DB_SECRET_ID        = data.terraform_remote_state.database.outputs.db_secret_arn
      JWT_ISSUER          = var.jwt_issuer
      JWT_AUDIENCE        = var.jwt_audience
      JWT_EXPIRES_MINUTES = tostring(var.jwt_expires_minutes)
    }
  }
}

resource "aws_lambda_function" "authorizer" {
  function_name    = "${local.name}-authorizer"
  role             = aws_iam_role.lambda.arn
  runtime          = "nodejs20.x"
  handler          = "authorizer.handler"
  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)
  timeout          = 10
  memory_size      = 128

  environment {
    variables = {
      JWT_SECRET_ID = var.jwt_secret_id
      DB_SECRET_ID  = data.terraform_remote_state.database.outputs.db_secret_arn
      JWT_ISSUER    = var.jwt_issuer
      JWT_AUDIENCE  = var.jwt_audience
    }
  }
}

# ───────────────────────── API Gateway (HTTP API) ─────────────────────────
resource "aws_apigatewayv2_api" "this" {
  name          = local.name
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_stage" "this" {
  api_id      = aws_apigatewayv2_api.this.id
  name        = "$default"
  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.apigw.arn
    format = jsonencode({
      requestId     = "$context.requestId"
      ip            = "$context.identity.sourceIp"
      routeKey      = "$context.routeKey"
      status        = "$context.status"
      responseLat   = "$context.responseLatency"
      correlationId = "$context.requestId"
    })
  }
}

resource "aws_cloudwatch_log_group" "apigw" {
  name              = "/aws/apigw/${local.name}"
  retention_in_days = 14
}

# Integração da rota pública de autenticação → Lambda de auth.
resource "aws_apigatewayv2_integration" "auth" {
  api_id                 = aws_apigatewayv2_api.this.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.auth.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "auth" {
  api_id    = aws_apigatewayv2_api.this.id
  route_key = "POST /auth"
  target    = "integrations/${aws_apigatewayv2_integration.auth.id}"
}

resource "aws_lambda_permission" "auth" {
  statement_id  = "AllowAPIGatewayInvokeAuth"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.auth.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.this.execution_arn}/*/*"
}

# Lambda Authorizer (SIMPLE) para validar o JWT HS256 nas rotas /api/*.
resource "aws_apigatewayv2_authorizer" "jwt" {
  api_id                            = aws_apigatewayv2_api.this.id
  authorizer_type                   = "REQUEST"
  authorizer_uri                    = aws_lambda_function.authorizer.invoke_arn
  identity_sources                  = ["$request.header.Authorization"]
  name                              = "${local.name}-jwt-authorizer"
  authorizer_payload_format_version = "2.0"
  enable_simple_responses           = true
}

resource "aws_lambda_permission" "authorizer" {
  statement_id  = "AllowAPIGatewayInvokeAuthorizer"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.authorizer.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.this.execution_arn}/*"
}

# Integração privada (VPC Link → NLB interno do app no EKS).
resource "aws_apigatewayv2_vpc_link" "eks" {
  name               = "${local.name}-vpclink"
  subnet_ids         = data.terraform_remote_state.k8s.outputs.private_subnet_ids
  security_group_ids = [data.terraform_remote_state.k8s.outputs.nodes_security_group_id]
}

resource "aws_apigatewayv2_integration" "app" {
  api_id                 = aws_apigatewayv2_api.this.id
  integration_type       = "HTTP_PROXY"
  integration_method     = "ANY"
  integration_uri        = data.terraform_remote_state.k8s.outputs.app_listener_arn
  connection_type        = "VPC_LINK"
  connection_id          = aws_apigatewayv2_vpc_link.eks.id
  payload_format_version = "1.0"
}

# Rotas protegidas: exigem JWT validado pelo Lambda Authorizer.
resource "aws_apigatewayv2_route" "app" {
  api_id             = aws_apigatewayv2_api.this.id
  route_key          = "ANY /api/{proxy+}"
  target             = "integrations/${aws_apigatewayv2_integration.app.id}"
  authorization_type = "CUSTOM"
  authorizer_id      = aws_apigatewayv2_authorizer.jwt.id
}
