using Repositories.Entities;

namespace Repositories.Repos.ChatRepos
{
    public interface IPhotoRepository
    {
        Task AddPhotoAsync(Photo photo);
        Task DeletePhotoAsync(Photo photo);
        Task<List<Photo>> GetPhotoFromMessageId(int photoId);
        Task<Photo> GetPhotoById(int msgId);
    }
}
