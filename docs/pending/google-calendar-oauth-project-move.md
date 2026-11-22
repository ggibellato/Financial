# Move the Google Calendar OAuth client to the correct project

Status: **Done** on 2026-11-22. Noted 2026-10-17.

## Result

- The calendar integration now uses the OAuth client **Financial Web Client** (type **Web application**) in Google Cloud project number **451120232237** (`financial-397307`).
- The consent screen is **External** and **In production**, so refresh tokens no longer expire after 7 days. It is unverified, so signing in shows Google's "unverified app" warning; choose **Advanced → Go to Financial (unsafe)**.
- The consent screen's Branding needed an application home page, a privacy policy link and an authorized domain before **Publish app** was enabled. It uses `https://github.com/ggibellato/financial`, `https://github.com/ggibellato/financial/blob/main/PRIVACY.md` and `github.com`.
- Authorized redirect URIs:
  - `http://localhost:8080/api/v1/financial/integrations/calendar/callback` (deployed app)
  - `http://localhost:5190/api/v1/financial/integrations/calendar/callback` (local dev)

  A Web application client accepts only these exact addresses, so changing the app's port means adding the new callback here too.
- The client ID and secret are in `scripts/deploy.local.json` (`GoogleCalendarClientId`, `GoogleCalendarClientSecret`) and `.env`. Both files are git-ignored; never commit the values.
- After the redeploy the calendar was reconnected and changing a credit card due date updated its existing calendar event.
- The old **Financial** Desktop client in project 215432212954 is deleted.

## Still to confirm

- The calendar stays connected for more than 7 days without reconnecting (check after 2026-11-29).
