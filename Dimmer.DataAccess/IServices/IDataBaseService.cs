using Realms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DataAccess.IServices;
public interface IDataBaseService
{
    Realm GetRealm();
    void DeleteDB();
}
