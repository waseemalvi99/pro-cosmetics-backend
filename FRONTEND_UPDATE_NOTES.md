# Frontend Updates Required — User Registration & Management

## 1. Register Endpoint Moved (Breaking Change)

**Old:** `POST /api/auth/register` (public, no auth required)
**New:** `POST /api/users/register` (requires authentication + `UserManagement:Create` permission)

### Request Body Changed

**Old:**
```json
{
  "fullName": "string",
  "email": "string",
  "password": "string",
  "roleName": "string | null"
}
```

**New:**
```json
{
  "fullName": "string",
  "email": "string",
  "roleName": "string | null"
}
```

- `password` field has been **removed**. The backend auto-generates a secure password and emails it to the new user.
- The request must include the JWT `Authorization: Bearer <token>` header.

### Response

Same `UserDto` response as before. Success message changed to: `"User created successfully. Credentials sent via email."`

### Frontend Actions Required

- Remove any public registration page/flow. Only authenticated admins with `UserManagement:Create` permission can create users.
- Move the "Create User" form into the User Management section of the admin panel.
- Remove the password field from the create user form. Only collect: **Full Name**, **Email**, and **Role** (optional dropdown).
- Update the API call URL from `/api/auth/register` to `/api/users/register`.
- Show a success message indicating credentials were sent to the user's email.

---

## 2. Toggle Active — Self-Deactivation Blocked

**Endpoint:** `PUT /api/users/{id}/toggle-active` (unchanged)

### New Behavior

- If an admin tries to deactivate **their own account**, the API returns **400 Bad Request**:
```json
{
  "success": false,
  "message": "You cannot deactivate your own account."
}
```

### Frontend Actions Required

- Hide or disable the activate/deactivate toggle button when the listed user is the currently logged-in user.
- Alternatively, handle the 400 error response and display the error message.

---

## 3. Summary of Permission Requirements

| Action | Endpoint | Permission |
|---|---|---|
| Create User | `POST /api/users/register` | `UserManagement:Create` |
| List Users | `GET /api/users/` | `UserManagement:View` |
| Get User | `GET /api/users/{id}` | `UserManagement:View` |
| Assign Role | `POST /api/users/{id}/assign-role` | `UserManagement:Edit` |
| Toggle Active | `PUT /api/users/{id}/toggle-active` | `UserManagement:Edit` |
