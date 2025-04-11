import { ConnectionPool } from 'mssql';
import { DefaultAzureCredential, TokenCredential } from '@azure/identity';

export class SqlConnectionWithAuthentication {
  private connectionString: string;
  private managedIdentityClientId?: string;
  private useManagedIdentity: boolean;

  constructor(connectionString: string, managedIdentityClientId?: string, isCloudEnvironment: boolean = false) {
    this.connectionString = connectionString;

    this.useManagedIdentity = isCloudEnvironment && !!managedIdentityClientId;
    this.managedIdentityClientId = this.useManagedIdentity ? managedIdentityClientId : undefined;
  }

  public async getOpenConnection(): Promise<ConnectionPool> {
    const config: any = {
      options: {
        encrypt: true,
      },
    };

    if (this.useManagedIdentity) {
      const credential: TokenCredential = new DefaultAzureCredential({
        managedIdentityClientId: this.managedIdentityClientId,
      });

      const tokenResponse = await credential.getToken('https://database.windows.net/.default');
      config.authentication = {
        type: 'azure-active-directory-access-token',
        options: {
          token: tokenResponse?.token,
        },
      };
    } else {
      config.connectionString = this.connectionString;
    }

    const pool = new ConnectionPool(config);
    await pool.connect();
    return pool;
  }
}
