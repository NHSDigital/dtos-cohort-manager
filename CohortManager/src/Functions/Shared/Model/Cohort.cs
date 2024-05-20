namespace Model;

public class Cohort
{
    public Cohort()
    {
        cohort = new List<Participant>();
    }
    public List<Participant> cohort { get; set; }
}
