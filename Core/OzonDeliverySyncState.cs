
namespace ApiOzon
{

    public class OzonDeliverySyncState
    {
        public int Id { get; set; }

        public string? Cursor { get; set; }

        public int Page { get; set; }

        public int TotalReceived { get; set; }

        public int TotalAdded { get; set; }

        public int TotalUpdated { get; set; }

        public int TotalSkipped { get; set; }

        public bool IsRunning { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public DateTime? LastSuccessAt { get; set; }

        public string? LastError { get; set; }
    }
}
