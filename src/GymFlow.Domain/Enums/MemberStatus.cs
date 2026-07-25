namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado del miembro como persona inscrita en el gimnasio. Es un flag de ciclo de
/// vida; los matices de morosidad/congelamiento viven en la <c>Membership</c> vigente.
/// </summary>
public enum MemberStatus
{
    Active = 1,
    Inactive = 2,
}
