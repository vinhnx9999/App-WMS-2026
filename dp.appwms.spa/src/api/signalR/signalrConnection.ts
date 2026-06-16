import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;
export const getSignalRConnection = (token: string): signalR.HubConnection => {
    if (connection) return connection;
    connection = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL}/hubs/wms`, {
            accessTokenFactory: () => token,
            skipNegotiation: false,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();
    return connection;
};