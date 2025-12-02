# Share Link Architecture Overview

This document outlines the high level architecture for the secure short link sharing feature.

## Database Objects

The PostgreSQL migration defined in `database/07_share.sql` introduces the `file.share_link` table for share metadata, `file.share_access_event` for auditing, and the `file.share_stats` materialized view for aggregated statistics. The schema follows the requirements in the product specification, including validity windows, quotas, password hashing storage, and IP allow lists. These objects are now managed by the Document module (while still using the `file` schema) so that link sharing is aligned with document ownership and lifecycle rules.

## Short Code Generation

Short link codes use a cryptographically strong base62 alphabet. The `Shared.Utilities.ShortCode.ShortCodeGenerator` class provides helpers to generate random codes between 6 and 16 characters as well as deterministic conversion from binary identifiers (e.g. ULIDs) to shorter textual representations.

## API & Backend Responsibilities

* Resolve `code → share_link.id` with uniqueness enforced at the database level.
* Expose `/api/ecm/shares` CRUD endpoints for administrators to create, update, revoke, and inspect share links together with `/shares/{id}/stats` materialized view lookups.
* Provide public endpoints `/s/{code}` (interstitial), `/s/{code}/password`, `/s/{code}/presign`, and `/s/{code}/download` that orchestrate password validation, quota tracking, presigned URL generation, and safe redirects.
* Validate revocation, validity window, subject authorization, password checks, quota limits, and optional IP filtering before serving the interstitial or download redirect.
* Persist access events in `file.share_access_event` for every view/download/password failure and refresh the `file.share_stats` materialized view to drive reporting.
* Generate presigned GET URLs against MinIO for each approved download, with a configurable 5–15 minute TTL and without exposing storage URLs before authorization completes.

## Frontend Interstitial Flow

* Anonymous visitors access `/s/{code}` and the Next.js page fetches metadata via the backend interstitial API.
* When a share is password-protected, the client renders a password form, invokes `/s/{code}/password` for validation, and refreshes the interstitial state once the password is accepted.
* The download button calls `/s/{code}/presign`, receives `{ url, expiresAtUtc }`, and navigates the browser to the presigned URL so the file streams directly from MinIO.

## Security Considerations

* Passwords are stored with Argon2id.
* Rate limiting is applied per IP + code.
* Response headers enforce `X-Frame-Options: DENY`, `Cache-Control: no-store`, and `X-Content-Type-Options: nosniff`.
* Presigned URLs are short lived and scoped to GET only.

## Future Work

* Surface share statistics and management tooling in the administration UI.
* Automate refresh of the materialized view after access events.
* Integrate watermarking and more granular IP allow list enforcement in the download flow.
