# New cloud project verification — 2026-11-17
Project: TT奥德赛 / b62afeec-3665-4c28-8c14-211eeb721a43
Organization: 33264422658385 (screenshot display name TT Odessey).
Anonymous authentication: PASS.
Relay allocation maxConnections=3: PASS.
Relay join code: PASS.
Second independent anonymous player join allocation via short code: PASS.
Initial CLI probe used incorrect data.allocationId and received 400; corrected to SDK schema data.allocation.allocationId and full API flow passed.
Tokens, codes and connection keys were not saved.
This verifies HTTPS control APIs only. Unity runtime reload, UDP/DTLS connection, gameplay, cross-network play and new build remain unverified.
