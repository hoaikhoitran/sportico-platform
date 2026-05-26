using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class PostMappingExtensions
    {
        public static Post ToEntity(
            this CreatePostRequest request,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            var post = new Post
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                SportId = request.SportId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                Price = request.Price,
                Location = request.Location?.Trim(),
                IsOnline = request.IsOnline,
                Status = PostStatusConstants.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            for (var i = 0; i < request.ImageUrls.Count; i++)
            {
                post.PostImages.Add(new PostImage
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    ImageUrl = request.ImageUrls[i],
                    OrderIndex = i,
                    CreatedAt = now
                });
            }

            return post;
        }

        public static void ApplyUpdate(
            this Post post,
            UpdatePostRequest request)
        {
            post.SportId = request.SportId;
            post.Title = request.Title.Trim();
            post.Description = request.Description?.Trim();
            post.Price = request.Price;
            post.Location = request.Location?.Trim();
            post.IsOnline = request.IsOnline;
            post.Status = PostStatusConstants.Pending;
            post.UpdatedAt = DateTime.UtcNow;

            post.PostImages.Clear();

            for (var i = 0; i < request.ImageUrls.Count; i++)
            {
                post.PostImages.Add(new PostImage
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    ImageUrl = request.ImageUrls[i],
                    OrderIndex = i,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public static PostResponse ToResponse(this Post post)
        {
            return new PostResponse
            {
                Id = post.Id,
                CoachId = post.CoachId,
                SportId = post.SportId,
                SportName = post.Sport?.Name ?? string.Empty,
                Title = post.Title,
                Description = post.Description,
                Price = post.Price,
                Location = post.Location,
                IsOnline = post.IsOnline,
                Status = post.Status,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ImageUrls = post.PostImages
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => x.ImageUrl)
                    .ToList()
            };
        }
    }
}
