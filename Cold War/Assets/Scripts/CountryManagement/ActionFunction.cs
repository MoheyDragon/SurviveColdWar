public class ActionFunction
{
    public string name;
    public int power, peoplSatsfaction,time;
    public float money;
    public bool Monthly;
    public string done;
    public ActionFunction( string _name,string _done, int _power, int _peoplSatsfaction, int _time, float _money,bool _monthly)
    {
        name = _name;
        done = _done;
        power = _power;
        peoplSatsfaction = _peoplSatsfaction;
        money = _money;
        time = _time;
        Monthly = _monthly;
    }
}
