namespace backend.models
{
    public class MatchRequest
    {
      
            public string ProfileText { get; set; } 
     
    }


    public class CandidateInput
    {
        public string Text { get; set; }
    }

    public class CandidateWithScore
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public float Score { get; set; }
    }
}
