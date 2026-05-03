<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\User;

class FriendshipController extends Controller {
    public function sendRequest($friendId) {
        auth()->user()->sentRequests()->attach($friendId, ['status'=>'pending']);


        
        return back()->with('success', 'Friend request sent!');
    }


    public function acceptRequest($friendId)
    {
        auth()->user()->receivedRequests()->updateExistingPivot($friendId, ['status' => 'accepted']);
        return back()->with('success', 'Friend request accepted!');
    }

    public function removeFriend($friendId)
    {
        auth()->user()->sentRequests()->detach($friendId);
        auth()->user()->receivedRequests()->detach($friendId);
        return back()->with('success', 'Friend removed.');
    }

}