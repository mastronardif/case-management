
use CaseManagement
-- Order matters: children before parents (FKs)
DELETE FROM cases.QueueClaimSession;
DELETE FROM cases.queueClaimsToBeCreated;
DELETE FROM cases.queueClaimsToBeSubmitted ;
DELETE FROM cases.ClaimSession;
DELETE FROM cases.Claim;

-- Optional: reset identities so new claims/queue rows start back at 1
DBCC CHECKIDENT ('cases.QueueClaimSession', RESEED, 0);
DBCC CHECKIDENT ('cases.queueClaimsToBeCreated', RESEED, 0);
DBCC CHECKIDENT ('cases.queueClaimsToBeSubmitted', RESEED, 0);
DBCC CHECKIDENT ('cases.ClaimSession', RESEED, 0);
DBCC CHECKIDENT ('cases.Claim', RESEED, 0);

/******
SELECT * FROM cases.QueueClaimSession;
SELECT * FROM cases.queueClaimsToBeCreated;
SELECT * FROM cases.queueClaimsToBeSubmitted;
SELECT * FROM cases.ClaimSession;
SELECT * FROM cases.Claim;

USE CaseManagement;
-- Order matters: children before parents (FKs). Scoped to QueueClaimId 5 / ClaimId 5 only —
-- your original had no WHERE on queueClaimsToBeSubmitted/ClaimSession/Claim, which would have
-- wiped every claim across every case, not just this one.
DECLARE @CaseId  INT = 9
DELETE FROM cases.QueueClaimSession        WHERE QueueClaimId = @CaseId ;
DELETE FROM cases.queueClaimsToBeCreated   WHERE QueueClaimId = @CaseId ;
DELETE FROM cases.queueClaimsToBeSubmitted WHERE ClaimId = @CaseId;
DELETE FROM cases.ClaimSession             WHERE ClaimId = @CaseId;
DELETE FROM cases.Claim                    WHERE ClaimId = @CaseId;
*******/