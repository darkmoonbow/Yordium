<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\User;
use App\Models\Message;

class UserController extends Controller
{
    public function show($id)
    {
        $user = User::findOrFail($id);

        return view('users.show', compact('user'));
    }

    public function updateStatus(Request $request) {
        $request->validate([
            'status' => 'required|string|max:255'
        ]);
        
        $user = auth()->user();
        $user->status = $request->status;
        $user->save();

        return back()->with('success', 'Status updated!');
    }
    public function sendMessage(Request $request) {
        $request->validate([
            'account_id' => 'required|integer|exists:users,id',
            'message' => 'required|string|max:1023'
        ]);

        if ($request->account_id == auth()->id()) {
            return back()->withErrors(['account_id' => 'You cannot send a message to yourself.']);
        }

        $message = Message::create([
            'account_id' => $request->account_id,
            'sender' => auth()->id(),
            'message' => $request->message,
            'is_read' => false

        ]);
        $message->update(['thread_id' => $message->id]);

        \Log::info($request->all());

        return back()->with('success', 'Message sent!');
    }
    public function replyMessage(Request $request, $threadId)
    {
        $request->validate([
            'message' => 'required|string|max:1023'
        ]);

        $originalMessage = Message::where('thread_id', $threadId)->firstOrFail();

        Message::create([
            'account_id' => $originalMessage->sender === auth()->id()
                ? $originalMessage->account_id
                : $originalMessage->sender,
            'sender' => auth()->id(),
            'message' => $request->message,
            'thread_id' => $originalMessage->thread_id,
            'is_read' => false,
        ]);

        return back()->with('success', 'Reply sent!');
    }

}
