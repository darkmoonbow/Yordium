<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Carbon\Carbon;

class HomePageController extends Controller
{
public function index()
    {
        $user = Auth::user();

        // update last active timestamp
        $user->updated_at = Carbon::now();
        $user->save();

        // get all accepted friends
        $friends = $user->friends();

        // get 6 most recent users
        $recentUsers = \App\Models\User::orderBy('created_at', 'desc')->take(6)->get();

        return view('home', [
            'user' => $user,
            'friends' => $friends,
            'username' => $user->username,
            'bits' => $user->bits,
            'bytes' => $user->bytes,
            'id' => $user->id
        ]);


    }

}
