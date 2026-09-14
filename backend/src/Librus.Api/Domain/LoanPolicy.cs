namespace Librus.Api.Domain;

public static class LoanPolicy
{
    public const int LoanPeriodDays = 28;
    public const int RenewalDays = 7;
    public const int MaxRenewals = 2;
    public const int MaxActiveLoansPerUser = 5;
}