namespace RestaurantPOS.Domain.Enums;

public enum OrderStatus
{
    Draft, // Waiter  and Order Always Must be it first state as a Draft.
    Submitted, // Watiting for Kitchen
    Preparing, // Kitchen
    Ready, // Kitchen finished
    Collected, // Waiter
    Served, // Waiter
}
