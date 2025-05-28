// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("ZqY/PGJRT1wa2nu/57sqguKGEpdimXYM4VwovUnCeLKPK9RJwQZlnGXomt5HF5d+CavGNb8hJwq9PCZGIZMQMyEcFxg7l1mX5hwQEBAUERJPOYrOm/pHhybjYeUwQ+8kOK1tS5MQHhEhkxAbE5MQEBGhuglyT3/ykwDjKUhSzdeEcZ/y8cRlecG9kZxUEC6z4Ma0OdU8FT9yJLTu9l6rn5/QFryjmmLg5rc2q6ms9fTqOXJucvwBppgXFF/Wkr3WhPmdI3dvO0//y76n7aXfanEH002nwObOwULGG0GZHVEj7O9QkBWUhA5KVJ0lroWRpqBDH24LA3kaJSz6vm33zluMYysPbfgCkvsVnrj8jz9rrFYz9D8EXzzgW0FGcQFznBMSEBEQ");
        private static int[] order = new int[] { 9,10,7,9,13,10,11,10,10,10,11,12,13,13,14 };
        private static int key = 17;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
