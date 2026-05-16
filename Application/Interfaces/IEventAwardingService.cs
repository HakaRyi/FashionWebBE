namespace Application.Interfaces
{
    public interface IEventAwardingService
    {
        Task FinalizeAndAwardEventAsync(int eventId);
    }
}
