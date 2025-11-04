# Laurel Library Subscription Setup Guide

This guide will help you set up the subscription-based payment system with Stripe integration.

## Overview

The subscription system includes three tiers:

1. **Bookworm Basic** (Free)
   - Up to 100 books
   - Up to 10 readers
   - Basic features

2. **Library Lover** ($29.99/month or $299.99/year)
   - Up to 1,000 books
   - Up to 100 readers
   - Advanced semantic search

3. **Bibliotheca Pro** ($79.99/month or $799.99/year)
   - Unlimited books and readers
   - Advanced semantic search
   - AI-powered age classification
   - Priority support

## Setup Instructions

### 1. Stripe Configuration

1. Create a Stripe account at https://stripe.com
2. Get your API keys from the Stripe Dashboard
3. Create products and prices in Stripe:

#### Library Lover Plan
- Create a product named "Library Lover"
- Create two prices:
  - Monthly: $29.99 (note the price ID)
  - Yearly: $299.99 (note the price ID)

#### Bibliotheca Pro Plan
- Create a product named "Bibliotheca Pro" 
- Create two prices:
  - Monthly: $79.99 (note the price ID)
  - Yearly: $799.99 (note the price ID)

4. Set up webhooks:
   - Endpoint URL: `https://yourdomain.com/administration/subscriptions/stripewebhook`
   - Events to listen for:
     - `checkout.session.completed`
     - `invoice.payment_succeeded`
     - `invoice.payment_failed`
     - `customer.subscription.updated`
     - `customer.subscription.deleted`

### 2. Update Configuration

Update your `appsettings.json` with Stripe configuration:

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

### 3. Update Stripe Price IDs

In `/LaurelLibrary.Domain/Entities/SubscriptionPlan.cs`, update the Stripe price IDs with your actual price IDs from Stripe:

```csharp
// Library Lover Plan
StripeMonthlyPriceId = "price_1234567890abcdef", // Replace with your actual price ID
StripeYearlyPriceId = "price_abcdef1234567890"   // Replace with your actual price ID

// Bibliotheca Pro Plan  
StripeMonthlyPriceId = "price_fedcba0987654321", // Replace with your actual price ID
StripeYearlyPriceId = "price_1357924680acbdef"   // Replace with your actual price ID
```

You can find these price IDs in your Stripe Dashboard under Products → [Product Name] → Pricing.

### 4. Database Migration

Run the following commands to create the subscription tables:

```bash
dotnet ef migrations add AddSubscriptionTables --project LaurelLibrary.Persistence --startup-project LaurelLibrary.UI
dotnet ef database update --project LaurelLibrary.Persistence --startup-project LaurelLibrary.UI
```

### 5. Create Initial Subscriptions

All existing libraries will automatically get a free "Bookworm Basic" subscription when they first access the subscription page.

## Features Implemented

### Subscription Limits
- Book creation is limited by subscription tier
- Reader creation is limited by subscription tier
- Semantic search requires Library Lover or Bibliotheca Pro
- Age classification requires Bibliotheca Pro

### UI Integration
- Subscription management page at `/Subscription`
- Usage indicators show current vs. allowed limits
- Upgrade prompts when limits are reached

### Automatic Features
- Webhook handling for Stripe events
- Subscription status updates
- Feature access control
- Usage tracking

## Testing

1. Test with Stripe test mode first
2. Use test credit card numbers from Stripe documentation
3. Verify webhook delivery in Stripe Dashboard
4. Test subscription limits by creating books/readers

## Production Deployment

1. Switch to Stripe live mode
2. Update webhook endpoint to production URL
3. Use live API keys in production configuration
4. Test the complete flow with real payments

## Support

For issues with the subscription system:
1. Check Stripe webhook logs
2. Review application logs for subscription service errors
3. Verify database subscription records
4. Test with Stripe test mode to isolate issues