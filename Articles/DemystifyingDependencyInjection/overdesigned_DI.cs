/*
    Justin's notes:
    While this isn't the code written by the interviewee--it would be unethical to post it exactly--it is my recreation
    of how it was put together, roughly. For context, the task was to create an application that could output whether a
    driver would receive a large ticket, medium ticket, small ticket, or no ticket. The decision was to be based on the
    speed limit, the driver's speed, and whether it was the driver's birthday or not (the driver got a 5 MPH break if 
    it was his or her birthday). That's it.

    To be fair to the guy, he knew his interview was for a senior-level position. We told him to do whatever made sense when 
    it came to the architecture of the application. He was probably just trying to show off.
*/

public static void Main(string[] args)
{
    IDateSystem dateSystem = new DefaultDateSystem();
    ICalendarProvider calendar = new DefaultCalendar(dateSystem);
    List<ITrafficLaw> laws = CreateDefaultTrafficLaws(calendar);

    ILegalEntity driver = new Citizen(new DateTime(1988, 1, 1), "John McClane");
    ITicketer officer = new Officer(calendar, "Sgt. Al Powell", laws);

    // Sample data
    ITrafficState state = new TrafficState()
    {
        IncidentSpeed = 50,
        SpeedLimit = 35,
        IncidentLocation = "Santa Monica Blvd"
    };

    IEnumerable<ICitation> citations = officer.IssueCitations(state, driver);
    
    foreach (var citation in citations)
        citation?.PrintCitation();
}

private static List<ITrafficLaw> CreateDefaultTrafficLaws(ICalendarProvider calendar)
{
    var laws = new List<ITrafficLaw>();
    laws.Add(new SpeedingTrafficLaw(calendar));

    return laws;
}

/* -------------------------------------------------------- */
/*                      Implementation                      */
/*  Pretend all of the below classes are in separate files  */
/* -------------------------------------------------------- */

internal class DefaultDateSystem : IDateSystem
{
    public DateTime GetCurrentDate() => DateTime.Now;
}

internal class DefaultCalendar : ICalendarProvider
{
    private readonly IDateSystem _dateSystem;
    public DefaultCalendar(IDateSystem dateSystem)
    {
        _dateSystem = dateSystem ?? throw new ArgumentNullException(nameof(dateSystem));
    }
    
    public DateTime CurrentDate => _dateSystem.GetCurrentDate();
}

internal class Officer : ITicketer
{
    private readonly ICalendarProvider _calendar;
    public Officer(ICalendarProvider calendar, string name, List<ITrafficLaw> trafficLaws)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TrafficLaws = trafficLaws ?? throw new ArgumentNullException(nameof(trafficLaws));
    }

    public IReadOnlyCollection<ITrafficLaw> TrafficLaws { get; }
    public string Name { get; }

    public IEnumerable<ICitation> IssueCitations(ITrafficState trafficState, ILegalEntity citee)
    {
        List<ICitation> citations;
        
        citations = TrafficLaws
            .Where(x => x.ShouldIssueCitation(trafficState, citee))
            .Select(x => x.CreateCitation(trafficState, citee, this))
            .ToList();
            
        return citations;
    }
}

internal class Citizen : ILegalEntity
{
    public Citizen(DateTime birthDate, string name)
    {
        BirthDate = birthDate;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public DateTime BirthDate { get; }
    public string Name { get; }
}

internal class SpeedingCitation : ICitation
{
    private readonly ICalendarProvider _calendar;
    public SpeedingCitation(ICalendarProvider calendar, ITicketer issuer, ILegalEntity citee, CitationSeverity severity)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        Citee = citee ?? throw new ArgumentNullException(nameof(citee));
        Severity = severity;
        CitationDate = _calendar.CurrentDate;
    }
    
    public DateTime CitationDate { get; }
    public ITicketer Issuer { get; }
    public ILegalEntity Citee { get; }
    public CitationSeverity Severity { get; }
    
    public void PrintCitation()
    {
        // assume something more sophisticated
        Console.WriteLine($"{Issuer.Name} issued {Severity} citation to {Citee.Name} on {CitationDate}.");
    }
}

internal class SpeedingTrafficLaw : ITrafficLaw
{
    private readonly ICalendarProvider _calendar;
    public SpeedingTrafficLaw(ICalendarProvider calendar)
    {
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }
    
    public bool ShouldIssueCitation(ITrafficState trafficState, ILegalEntity citee)
    {
        var severity = CalculateSeverity(trafficState, citee);			
        return severity != CitationSeverity.None;
    }

    public ICitation CreateCitation(ITrafficState trafficState, ILegalEntity citee, ITicketer issuer)
    {			
        return new SpeedingCitation(_calendar, issuer, citee, CalculateSeverity(trafficState, citee));
    }
    
    private CitationSeverity CalculateSeverity(ITrafficState trafficState, ILegalEntity citee)
    {
        int speedDiff = trafficState.IncidentSpeed - trafficState.SpeedLimit;

        // If it's the citee's birthday, knock 5 MPH off of their speed diff.
        if (citee.BirthDate.Month == _calendar.CurrentDate.Month &&
            citee.BirthDate.Day == _calendar.CurrentDate.Day)
        {
            speedDiff -= 5;
        }

        CitationSeverity severity;

        if (speedDiff > 25)
            severity = CitationSeverity.Large;
        else if (speedDiff > 10)
            severity = CitationSeverity.Medium;
        else if (speedDiff > 0)
            severity = CitationSeverity.Small;
        else
            severity = CitationSeverity.None;
            
        return severity;
    }
}

internal class TrafficState : ITrafficState
{
    public int IncidentSpeed { get; set; }
    public int SpeedLimit { get; set; }
    public string IncidentLocation { get; set; }
}

/* --------------------------------------------------------- */
/*                        Interfaces                         */
/* Pretend all of the below interfaces are in separate files */
/* --------------------------------------------------------- */

public enum CitationSeverity
{
    None,
    Small,
    Medium,
    Large
}

public interface ITicketer
{
    IReadOnlyCollection<ITrafficLaw> TrafficLaws { get; }
    string Name { get; }
    IEnumerable<ICitation> IssueCitations(ITrafficState trafficState, ILegalEntity citee);
}

public interface ITrafficLaw
{
    bool ShouldIssueCitation(ITrafficState trafficState, ILegalEntity citee);
    ICitation CreateCitation(ITrafficState trafficState, ILegalEntity citee, ITicketer issuer);
}

public interface ITrafficState
{
    int IncidentSpeed { get; }
    int SpeedLimit { get; }
    string IncidentLocation { get; }    // assume something more sophisticated
}

public interface ICalendarProvider
{
    DateTime CurrentDate { get; }
}

public interface IDateSystem
{
    DateTime GetCurrentDate();
}

public interface ILegalEntity
{
    string Name { get; }
    DateTime BirthDate { get; }
}

public interface ICitation
{
    DateTime CitationDate { get; }
    ITicketer Issuer { get; }
    ILegalEntity Citee { get; }
    CitationSeverity Severity { get; }

    void PrintCitation();
}
