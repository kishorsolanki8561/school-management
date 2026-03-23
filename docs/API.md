# API Reference

Base URL: `https://{host}/api/v1`

All protected endpoints require: `Authorization: Bearer {accessToken}`

### Audit Tracking Header

To record which screen triggered a change, include this header on any write request (POST / PUT / DELETE):

```
X-Screen-Name: Country Management
```

The value is stored in `AuditLogs.ScreenName`. If omitted the field is stored as `null`.

---

## Response Format

### Success
```json
{
  "success": true,
  "message": "string",
  "data": { ... },
  "errors": null
}
```

### Error
```json
{
  "success": false,
  "message": "string",
  "data": null,
  "errors": [
    { "field": "Name", "message": "Name is required" }
  ]
}
```

### Paginated Response (`data` field)
```json
{
  "items": [ ... ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

---

## Auth Module

### POST `/auth/login`
Authenticate and receive tokens.

**Request**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response `data`**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "accessTokenExpiry": "2026-03-23T10:00:00Z",
  "username": "johndoe",
  "role": "SchoolAdmin"
}
```

---

### POST `/auth/register`
Register a new user account.

**Request**
```json
{
  "username": "string",
  "email": "string",
  "password": "string",
  "role": "Student | Teacher | SchoolAdmin | Supervisor | SuperAdmin"
}
```

---

### POST `/auth/refresh`
Exchange a valid refresh token for new tokens.

**Request**
```json
{
  "refreshToken": "string"
}
```

**Response `data`** — same shape as login response.

---

### POST `/auth/logout`
Revoke the current refresh token. **Requires auth.**

**Request**
```json
{
  "refreshToken": "string"
}
```

---

### POST `/auth/forgot-password`
Request a password reset email.

**Request**
```json
{
  "email": "string"
}
```

---

### POST `/auth/reset-password`
Reset password using the emailed token.

**Request**
```json
{
  "token": "string",
  "newPassword": "string"
}
```

---

## Country Module

All endpoints require auth (`Bearer`).

### POST `/country`
Create a new country.

**Request**
```json
{
  "name": "India",
  "code": "IND"
}
```

**Response `data`** — `CountryResponse` (see [MASTERS.md](MASTERS.md))

---

### PUT `/country/{id}`
Update an existing country.

**Request**
```json
{
  "name": "India",
  "code": "IND",
  "isActive": true
}
```

---

### DELETE `/country/{id}`
Soft-delete a country (`IsDeleted = true`).

---

### GET `/country/{id}`
Get a single country by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "India",
  "code": "IND",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/country`
Get paginated list of countries.

**Query Parameters**

| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |
| `search` | string | — | Filter by name or code |

---

## State Module

All endpoints require auth.

### POST `/state`
Create a state.

**Request**
```json
{
  "name": "Maharashtra",
  "code": "MH",
  "countryId": 1
}
```

---

### PUT `/state/{id}`
Update a state.

**Request**
```json
{
  "name": "Maharashtra",
  "code": "MH",
  "isActive": true
}
```

---

### DELETE `/state/{id}`
Soft-delete a state.

---

### GET `/state/{id}`
Get a state by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "Maharashtra",
  "code": "MH",
  "countryId": 1,
  "countryName": "India",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/state`
Get paginated list of states.

**Query Parameters** — same as `/country`.

---

### GET `/state/by-country/{countryId}`
Get all active states for a given country (no pagination).

---

## City Module

All endpoints require auth.

### POST `/city`
Create a city.

**Request**
```json
{
  "name": "Mumbai",
  "stateId": 1
}
```

---

### PUT `/city/{id}`
Update a city.

**Request**
```json
{
  "name": "Mumbai",
  "isActive": true
}
```

---

### DELETE `/city/{id}`
Soft-delete a city.

---

### GET `/city/{id}`
Get a city by ID.

**Response `data`**
```json
{
  "id": 1,
  "name": "Mumbai",
  "stateId": 1,
  "stateName": "Maharashtra",
  "countryId": 1,
  "countryName": "India",
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### GET `/city`
Get paginated list of cities.

**Query Parameters** — same as `/country`.

---

### GET `/city/by-state/{stateId}`
Get all active cities for a given state (no pagination).

---

## Encryption Module

### GET `/encryption/public-key`
Returns the server's RSA-2048 public key in PEM format.
Used by clients to encrypt the AES session key when E2E encryption is enabled.

**Response `data`**
```
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
-----END PUBLIC KEY-----
```

---

## Endpoint Summary

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/auth/login` | No | Authenticate user |
| POST | `/auth/register` | No | Register user |
| POST | `/auth/refresh` | No | Refresh tokens |
| POST | `/auth/logout` | Yes | Revoke refresh token |
| POST | `/auth/forgot-password` | No | Send reset email |
| POST | `/auth/reset-password` | No | Reset password |
| POST | `/country` | Yes | Create country |
| PUT | `/country/{id}` | Yes | Update country |
| DELETE | `/country/{id}` | Yes | Delete country |
| GET | `/country/{id}` | Yes | Get country |
| GET | `/country` | Yes | List countries |
| POST | `/state` | Yes | Create state |
| PUT | `/state/{id}` | Yes | Update state |
| DELETE | `/state/{id}` | Yes | Delete state |
| GET | `/state/{id}` | Yes | Get state |
| GET | `/state` | Yes | List states |
| GET | `/state/by-country/{id}` | Yes | States by country |
| POST | `/city` | Yes | Create city |
| PUT | `/city/{id}` | Yes | Update city |
| DELETE | `/city/{id}` | Yes | Delete city |
| GET | `/city/{id}` | Yes | Get city |
| GET | `/city` | Yes | List cities |
| GET | `/city/by-state/{id}` | Yes | Cities by state |
| GET | `/encryption/public-key` | No | Get RSA public key |
