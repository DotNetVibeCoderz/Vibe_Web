using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NetTube.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Video> Videos { get; set; } = new();
        public List<Subscription> Subscriptions { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        public List<WatchHistory> WatchHistories { get; set; } = new();
    }

    public class Video
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? Category { get; set; }
        public int Views { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User? Uploader { get; set; }

        public List<Comment> Comments { get; set; } = new();
        public List<Like> Likes { get; set; } = new();
        public List<PlaylistVideo> PlaylistVideos { get; set; } = new();
        public List<WatchHistory> WatchHistories { get; set; } = new();
    }

    public class Comment
    {
        public int Id { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User? User { get; set; }
        public int VideoId { get; set; }
        public Video? Video { get; set; }
    }

    public class Like
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int VideoId { get; set; }
        public Video? Video { get; set; }
    }

    public class Subscription
    {
        public int Id { get; set; }
        public int SubscriberId { get; set; }
        public User? Subscriber { get; set; }
        public int CreatorId { get; set; }
        public User? Creator { get; set; }
    }

    public class CommunityPost
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? Creator { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }

    public class Playlist
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<PlaylistVideo> PlaylistVideos { get; set; } = new();
    }

    public class PlaylistVideo
    {
        public int Id { get; set; }
        public int PlaylistId { get; set; }
        public Playlist? Playlist { get; set; }
        public int VideoId { get; set; }
        public Video? Video { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class WatchHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int VideoId { get; set; }
        public Video? Video { get; set; }
        public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
    }
}