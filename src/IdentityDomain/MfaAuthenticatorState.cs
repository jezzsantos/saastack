namespace IdentityDomain;

public enum MfaAuthenticatorState
{
    Created = 0,
    Associated = 1, // The user is associating a new authenticator
    Confirmed = 2, // The user confirmed the association
    Challenged = 3, // The user has requested a challenge
    Verified = 4 // The user has verified the challenge
}