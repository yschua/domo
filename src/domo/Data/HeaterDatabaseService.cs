using LiteDB;

namespace domo.Data;

public class HeaterDatabaseService
{
    public HeaterDatabaseService(LiteDatabase db, HeaterFactory heaterFactory)
    {
        Heaters = db.GetCollection<Heater>();

        if (Heaters.Count() < 1)
        {
            Heaters.Insert(heaterFactory.Create());
        }
        else if (Heaters.Count() > 1)
        {
            throw new InvalidOperationException("Database corrupted");
        }

        Heater = Heaters.FindAll().FirstOrDefault();
        Heater.SetUpPropertyChangedHandler();
        Heater.RegisterUpdateHandler(Update);
    }

    public Heater Heater { get; }

    private ILiteCollection<Heater> Heaters { get; }

    public void Update()
    {
        Heaters.Update(Heater);
    }
}
