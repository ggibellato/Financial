# Move the Google Calendar OAuth client to the correct project

Status: pending. Noted 2026-10-17.

## Current state

- The calendar integration uses the OAuth client **Financial** (type **Desktop**, created 2026-09-07).
- It lives in Google Cloud project number **215432212954**, which is the wrong project. It works and reaches the correct calendar, so it stays as it is until the move.
- Being a Desktop client, it has no list of redirect URIs: Google accepts any `localhost` address, which is why both the old desktop app (`localhost:8082`) and the deployed web app (`localhost:8080/.../callback`) worked without registering them. There is nothing left to remove for the desktop app.
- The OAuth consent screen's **Publishing status is Testing**. In Testing, Google cancels refresh tokens after 7 days, which is why the calendar shows as disconnected about once a week until it is reconnected in the app.
- **Publish app** is greyed out. Likely causes: required consent screen fields missing (app name, user support email, developer contact email), or the user type is not External.

## Steps for the move

1. In the correct Google Cloud project:
   1. Enable the **Google Calendar API**.
   2. Set up the **OAuth consent screen**: user type **External**, app name, user support email and developer contact email. Homepage, privacy policy and logo can stay empty.
   3. **Publish** the app so its status is **In production**. Google may say the app needs verification; that is not needed for a personal app with under 100 users. Sign-in shows an "unverified app" warning instead.
   4. Create the **OAuth client**. Desktop keeps today's behaviour. If it is a **Web application** client instead, add the redirect URIs `http://localhost:8080/api/v1/financial/integrations/calendar/callback` (deployed app) and `http://localhost:5190/api/v1/financial/integrations/calendar/callback` (local dev).
2. Put the new client ID and secret into `scripts/deploy.local.json` (`GoogleCalendarClientId`, `GoogleCalendarClientSecret`) and `.env`. Both files are git-ignored; never commit the values.
3. Redeploy with `scripts/deploy.ps1` and start with `deploy/start-web.ps1`.
4. In the app at http://localhost:8080, disconnect the calendar and connect it again. On Google's "hasn't verified this app" screen choose **Advanced → Go to Financial (unsafe)** and allow access.
5. Check that a calendar event is created or updated (for example by saving a credit card due date).
6. Delete the old **Financial** client in project 215432212954.

## Done when

- The OAuth client is in the correct project and its consent screen is **In production**.
- The calendar stays connected for more than 7 days without reconnecting.
- The old client in project 215432212954 is deleted.
