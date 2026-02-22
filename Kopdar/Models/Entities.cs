using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kopdar.Models;

public class AppUser
{
    public int Id { get; set; }
    [Required]
    public string Username { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public string PasswordHash { get; set; }
    public string ProfilePictureUrl { get; set; } = "/images/logo.svg";
    public string? Bio { get; set; } = "Kopdar new user! Ready to chat.";
    public string? Gender { get; set; } = "Unspecified";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;

    // Relational
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
}

public class Follow
{
    public int Id { get; set; }
    public int FollowerId { get; set; }
    public AppUser Follower { get; set; }
    public int FollowingId { get; set; }
    public AppUser Following { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; }
    public string Content { get; set; }
    public string? MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public class Like
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; }
}

public class Comment
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string GroupPictureUrl { get; set; } = "https://cdn-icons-png.flaticon.com/512/32/32441.png";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsPublic { get; set; } = true;
    public int CreatorId { get; set; }
    public AppUser Creator { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public ChatGroup Group { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public AppUser Sender { get; set; }
    
    public int? ReceiverId { get; set; }
    public AppUser? Receiver { get; set; }
    
    public int? GroupId { get; set; }
    public ChatGroup? Group { get; set; }

    public string Content { get; set; }
    public string? MediaUrl { get; set; } // For image, video, audio, document
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; } // Receiver
    public AppUser User { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
