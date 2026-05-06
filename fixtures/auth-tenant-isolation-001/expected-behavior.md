# Expected behavior

GET /documents/{id} returns 200 only when the requested document belongs to the caller tenant. Cross-tenant access returns 404.
