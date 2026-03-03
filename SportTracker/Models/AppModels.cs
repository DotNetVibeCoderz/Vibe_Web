using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportTracker.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "user"; // admin, user
        public string Email { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<UserClub> UserClubs { get; set; } = new List<UserClub>();
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public ICollection<Follow> Following { get; set; } = new List<Follow>();
    }

    public class Activity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Run"; // Run, Ride, Walk, Hike, Swim
        public double DistanceKm { get; set; } // Kilometers
        public TimeSpan Duration { get; set; }
        public double ElevationGainMeters { get; set; }
        public double CaloriesBurned { get; set; }
        public double AverageHeartRate { get; set; }
        public double AveragePaceMinPerKm { get; set; } // minutes per km
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string MapRouteData { get; set; } = string.Empty; // JSON of coordinates or polyline
        public string Description { get; set; } = string.Empty;

        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class Comment
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity? Activity { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Like
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public Activity? Activity { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Club
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserClub> Members { get; set; } = new List<UserClub>();
    }

    public class UserClub
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ClubId { get; set; }
        public Club? Club { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class Follow
    {
        public int FollowerId { get; set; }
        public User? Follower { get; set; }
        public int FollowedId { get; set; }
        public User? Followed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Goal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Type { get; set; } = "Distance"; // Distance, Time, Calories
        public string ActivityType { get; set; } = "Run"; // Run, Ride, etc.
        public double TargetValue { get; set; }
        public double CurrentValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}