// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/WalletStatsDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

public class WalletStatsDto
{
    public int TotalWallets { get; set; }
    public int ActiveWallets { get; set; }
    public int EmptyWallets { get; set; }
    
    public decimal TotalBalance { get; set; }
    public int TotalPoints { get; set; }
    public decimal TotalValue { get; set; }
    public string Currency { get; set; } = "EGP";
    
    // Averages
    public decimal AverageBalance { get; set; }
    public int AveragePoints { get; set; }
    public decimal AverageValue { get; set; }
    
    // Transaction stats
    public int TotalTransactions { get; set; }
    public int CreditTransactions { get; set; }
    public int DebitTransactions { get; set; }
    public int PointsTransactions { get; set; }
    public int TransferTransactions { get; set; }
    
    // Top wallets
    public List<TopWalletDto> TopWalletsByBalance { get; set; } = new();
    public List<TopWalletDto> TopWalletsByPoints { get; set; } = new();
    
    // Recent activity
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
}

public class TopWalletDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int Points { get; set; }
    public string BalanceDisplay => $"{Balance:C}";
    public string PointsDisplay => $"{Points:N0} Points";
}

public class RecentTransactionDto
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public string AmountDisplay => Currency == "PTS" ? 
        $"{Amount:N0} Points" : 
        $"{Amount:C} {Currency}";
    
    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            return span.TotalMinutes < 1 ? "Just now" :
                   span.TotalHours < 1 ? $"{(int)span.TotalMinutes} minutes ago" :
                   span.TotalDays < 1 ? $"{(int)span.TotalHours} hours ago" :
                   $"{(int)span.TotalDays} days ago";
        }
    }
}