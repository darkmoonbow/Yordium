<?php

namespace App\Http\Controllers;
use Illuminate\Http\Request;

class ServerController extends Controller
{
    public function getServer()
    {
        $serversFile = storage_path('servers.json');
        $servers = file_exists($serversFile)
            ? json_decode(file_get_contents($serversFile), true)
            : [];

        // Find server with open slots
        foreach ($servers as &$server) {
            if ($server['players'] < 20) {
                return response()->json($server);
            }
        }

        // Start a new subserver
        $newPort = 6000 + count($servers);
        $server = [
            'ip' => getHostByName(getHostName()),
            'port' => 9050,
            'players' => 0
        ];

        // launch a new Stride headless server
        exec("start cmd /c YordiumServer.exe --port={$newPort}");

        $servers[] = $server;
        file_put_contents($serversFile, json_encode($servers));

        return response()->json($server);
    }
}
