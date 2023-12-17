using System;
namespace SurviveColdWar
{
    public struct Value : IEquatable<Value>
    {
        public int power;
        public float money;
        public float peopleSatisfaction;
        public Value(int power, float money, float peopleSatisfaction)
        {
            this.power = power;
            this.money = money;
            this.peopleSatisfaction = peopleSatisfaction;
        }

        public bool Equals(Value other)
        {
            throw new NotImplementedException();
        }
        public static Value operator +(Value a, Value b)
        {
            return new Value(a.power + b.power, a.money + b.money, a.peopleSatisfaction + b.peopleSatisfaction);
        }
    }
}
