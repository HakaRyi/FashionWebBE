namespace Services.Implements.EventAwardingImp
{
    public interface IEventAwardingService
    {
        Task FinalizeAndAwardEventAsync(int eventId);
    }
}
