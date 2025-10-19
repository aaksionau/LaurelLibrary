# Microsoft Account Authentication Setup

## Overview
Microsoft Account authentication has been added to your LaurelLibrary application. Users can now register and log in using their Microsoft accounts (Outlook, Hotmail, Office 365, etc.).

## What Was Changed

### 1. Package Installed
- `Microsoft.AspNetCore.Authentication.MicrosoftAccount` v9.0.10

### 2. Files Modified

#### Program.cs
Added Microsoft authentication configuration after Identity setup:
```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? throw new InvalidOperationException("Microsoft ClientId not configured");
        microsoftOptions.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? throw new InvalidOperationException("Microsoft ClientSecret not configured");
    });
```

#### appsettings.json
Added authentication configuration section:
```json
"Authentication": {
  "Microsoft": {
    "ClientId": "<!-- Your Microsoft App ClientId -->",
    "ClientSecret": "<!-- Your Microsoft App ClientSecret -->"
  }
}
```

### 3. Files Created

#### /Areas/Identity/Pages/Account/ExternalLogin.cshtml
- Handles the external login callback
- Displays a form for users to confirm their email when registering with Microsoft

#### /Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs
- Contains the logic for external authentication
- Handles user registration with Microsoft account
- Automatically extracts first name and last name from Microsoft claims
- Signs in existing users who have already linked their Microsoft account

## Setup Instructions

### Step 1: Register Your Application with Microsoft

1. Go to the **Azure Portal**: https://portal.azure.com/
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Fill in the registration form:
   - **Name**: `LaurelLibrary` (or your preferred name)
   - **Supported account types**: Select "Accounts in any organizational directory and personal Microsoft accounts"
   - **Redirect URI**: 
     - Platform: `Web`
     - URL: `https://localhost:5001/signin-microsoft` (for development)
     - Note: You'll need to add production URLs later
5. Click **Register**

### Step 2: Get Your Client ID and Client Secret

1. After registration, you'll see the **Application (client) ID** - this is your `ClientId`
2. Go to **Certificates & secrets** in the left menu
3. Click **New client secret**
4. Add a description (e.g., "LaurelLibrary Development")
5. Choose an expiration period
6. Click **Add**
7. **IMPORTANT**: Copy the **Value** immediately - this is your `ClientSecret` (you won't be able to see it again)

### Step 3: Configure Your Application

#### For Development (using User Secrets):
```bash
cd /home/alex/Code/LaurelLibrary/LaurelLibrary.UI

# Set the ClientId
dotnet user-secrets set "Authentication:Microsoft:ClientId" "YOUR_CLIENT_ID_HERE"

# Set the ClientSecret
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
```

#### For Production:
- Use Azure Key Vault or environment variables
- Never commit secrets to source control
- Update your production redirect URIs in the Azure Portal

### Step 4: Add Redirect URIs for Different Environments

In the Azure Portal, under your app registration:
1. Go to **Authentication** > **Platform configurations** > **Web**
2. Add redirect URIs for each environment:
   - Development: `https://localhost:5001/signin-microsoft`
   - Production: `https://yourdomain.com/signin-microsoft`

### Step 5: Test the Authentication

1. Start your application:
   ```bash
   dotnet watch
   ```

2. Navigate to the Register page
3. You should see a "Microsoft" button under "Use another service to register"
4. Click it to test the Microsoft authentication flow

## How It Works

### Registration Flow
1. User clicks "Microsoft" button on the Register page
2. User is redirected to Microsoft login
3. User logs in with their Microsoft account
4. Microsoft redirects back to your app with authentication info
5. If it's a new user:
   - The ExternalLogin page is displayed
   - User confirms their email address
   - A new account is created with their Microsoft account linked
   - First name and last name are automatically extracted from Microsoft profile
6. User is signed in

### Login Flow
1. User clicks "Microsoft" button on the Login page
2. User is redirected to Microsoft login
3. User logs in with their Microsoft account
4. If the Microsoft account is already linked to an existing user:
   - User is automatically signed in
5. If the Microsoft account is not linked:
   - The registration flow (above) is triggered

## Troubleshooting

### "Microsoft ClientId not configured" Error
- Make sure you've set the user secrets or configuration values
- Check that the keys are correctly named in your configuration

### Redirect URI Mismatch Error
- Ensure the redirect URI in Azure Portal exactly matches your application URL
- Format: `https://yourdomain.com/signin-microsoft`

### Unable to See External Login Button
- The button appears on both Register and Login pages
- If using default Identity pages, you may need to scaffold the Login page
- To scaffold: `dotnet aspnet-codegenerator identity -dc AppDbContext --files "Account.Login"`

## Additional Configuration Options

### Customize Scopes
You can request additional information from Microsoft:
```csharp
.AddMicrosoftAccount(microsoftOptions =>
{
    microsoftOptions.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    microsoftOptions.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    microsoftOptions.Scope.Add("User.Read");
    microsoftOptions.Scope.Add("email");
});
```

### Handle Account Linking
Users can link multiple external providers to their account by:
1. Logging in with their primary account
2. Going to their profile settings
3. Linking additional external accounts

## Security Best Practices

1. **Never commit secrets**: Always use User Secrets for development and secure vaults for production
2. **Use HTTPS**: External authentication requires HTTPS
3. **Validate redirect URIs**: Only whitelist your actual domain URIs in Azure Portal
4. **Rotate secrets**: Regularly rotate your client secrets
5. **Monitor authentication**: Keep track of external login usage in your logs

## Next Steps

- Consider adding Google, Facebook, or other OAuth providers
- Implement profile management for linking/unlinking external accounts
- Add custom claims mapping for additional user information
- Set up email confirmation for enhanced security

## References

- [Microsoft Account Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins)
- [Azure AD App Registration](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
