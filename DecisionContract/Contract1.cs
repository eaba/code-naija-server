//using Amazon.EC2.Model;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;

namespace DecisionContract
{
    public class Contract1 : SmartContract
    {
        
        public static bool Main(string operation, object[] args)
        {
            StorageMap candidates = Storage.CurrentContext.CreateMap("candidates"); // 'user' prefix
            StorageMap voters = Storage.CurrentContext.CreateMap("voters");
            StorageMap identities = Storage.CurrentContext.CreateMap("identities");
            if (operation == "RequestIdentity")
            {
                Runtime.Notify(operation, args);
                return true;
            }
            if (operation == "DeleteContract")
            {
                Contract.Destroy();
                Runtime.Notify(operation, args);
                return true;
            }
            if (operation == "CreateIdentity")
            {
                object[] ids = new object[5];
                ids[0] = args[0];    // public
                ids[1] = args[1];        // address
                ids[2] = args[2];        // id
                byte[] ids_storage = ids.Serialize();
                identities.Put((string)args[1], ids_storage);
                Runtime.Notify(operation, args);
            }
            if (operation == "AddCandidate")
            {
                if (candidates.Get((string)args[3]).Length != 0)
                {
                    Runtime.Log("Already a Added");
                    return false;
                }
                else
                {

                    object[] parties = new object[5];
                    parties[0] = args[0];    // public
                    parties[1] = args[1];        // address
                    parties[2] = args[2];        // name
                    parties[3] = args[3];        // party
                    parties[4] = "0";        // count
                    byte[] candidates_storage = parties.Serialize();
                    candidates.Put((string)args[3], candidates_storage);
                    Runtime.Notify(operation, args);
                    return true;
                }
            }
            if (operation == "Vote")
            {
                if (voters.Get((string)args[0]).Length != 0)
                {
                    Runtime.Log("Already a Voted");
                    return false;
                }
                else
                {
                    var vote_storage = candidates.Get((string)args[1]);
                    object[] vt = (object[])vote_storage.Deserialize();
                    vt[4] = (int)vt[4] + 1;

                    vote_storage = vt.Serialize();
                    candidates.Put((string)args[1], vote_storage);
                    Runtime.Notify(operation, args);
                    return true;
                }
            }
            return true;
        }
    }
}
