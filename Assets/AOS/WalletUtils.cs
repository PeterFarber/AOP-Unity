using UnityEngine;

namespace AO
{
    public static class WalletUtils
    {
        [System.Serializable]
        public class JWKInterface
        {
            public string kty;
            public string e;
            public string n;
            public string d;
            public string p;
            public string q;
            public string dp;
            public string dq;
            public string qi;
        }

        public static string ExtractWalletID(string jsonContent)
        {
            JWKInterface jwk = JsonUtility.FromJson<JWKInterface>(jsonContent);
            return Base64Utils.OwnerToAddress(jwk.n);
        }
    }
}
