output "api_endpoint" {
  description = "URL base do API Gateway (POST /auth e /api/*)."
  value       = aws_apigatewayv2_api.this.api_endpoint
}

output "auth_function_name" {
  value = aws_lambda_function.auth.function_name
}

output "authorizer_function_name" {
  value = aws_lambda_function.authorizer.function_name
}
