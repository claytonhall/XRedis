using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using XRedis.Core;

namespace XRedis.Identity
{
    public class XRedisIdentityContext : XRedisContext
    {
        public XRedisIdentityContext(IXRedisConnection connection) : base(connection)
        {
        }
    }


    
    
    
    
    
    public class XRedisIdentityUserBase : IdentityUser<long>
    {
    }

    public class XRedisIdentityRoleBase : IdentityRole<long>
    {
    }
    
    public class XRedisIdentityUserRoleBase : IdentityUserRole<long>
    {
    }

    public class XRedisIdentityRoleClaimBase : IdentityRoleClaim<long>
    {
    }

    public class XRedisIdentityUserClaimBase : IdentityUserClaim<long>
    {
    }

    public class XRedisIdentityUserLoginBase : IdentityUserLogin<long>
    {
    }

    public class XRedisIdentityUserTokenBase : IdentityUserToken<long>
    {
    }
}
