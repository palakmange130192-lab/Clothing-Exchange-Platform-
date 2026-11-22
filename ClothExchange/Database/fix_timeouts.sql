-- Run this ONCE if the app hangs or shows "timeout expired".
-- Stop the app in Visual Studio first (Shift+F5).

-- 1. Readers will no longer wait for writers (stops most "hanging page" problems)
USE master;
GO
ALTER DATABASE ClothExchangeDb SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

-- 2. (Optional) see if some old connection left a transaction open
--    If a row comes back, run: KILL <session_id>
USE ClothExchangeDb;
GO
SELECT session_id, login_name, status, open_transaction_count, last_request_start_time
FROM sys.dm_exec_sessions
WHERE is_user_process = 1 AND open_transaction_count > 0;
GO
