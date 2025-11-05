variable "project_name" {
  description = "Name of the project, used as prefix for all resources"
  type        = string
  default     = "laurel"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region where resources will be created"
  type        = string
  default     = "eastus"
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Project     = "MyLibrarian"
    ManagedBy   = "Terraform"
  }
}

variable "admin_email" {
  description = "Admin email address for the application"
  type        = string
  default     = "admin@mylibrarianapp.com"
}

# SQL Server variables
variable "sql_admin_username" {
  description = "Administrator username for SQL Server"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "Administrator password for SQL Server"
  type        = string
  sensitive   = true
}

# Microsoft Authentication variables
variable "microsoft_client_id" {
  description = "Microsoft OAuth Client ID"
  type        = string
  sensitive   = true
}

variable "microsoft_client_secret" {
  description = "Microsoft OAuth Client Secret"
  type        = string
  sensitive   = true
}

# Azure OpenAI variables
variable "openai_endpoint" {
  description = "Azure OpenAI endpoint URL"
  type        = string
  sensitive   = true
}

variable "openai_apikey" {
  description = "Azure OpenAI API key"
  type        = string
  sensitive   = true
}

# ISBNdb variables
variable "isbndb_apikey" {
  description = "ISBNdb API key"
  type        = string
  sensitive   = true
}

# Stripe variables
variable "stripe_publishable_key" {
  description = "Stripe publishable key"
  type        = string
  sensitive   = true
}

variable "stripe_secret_key" {
  description = "Stripe secret key"
  type        = string
  sensitive   = true
}

variable "stripe_webhook_secret" {
  description = "Stripe webhook secret"
  type        = string
  sensitive   = true
}
