<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\Message;
use App\Services\ReplyService;
use Illuminate\Support\Facades\Auth;

class InboxController extends Controller
{
    protected $replyService;

    public function __construct(ReplyService $replyService)
    {
        $this->replyService = $replyService;
    }

    // Show all messages for the authenticated user
    public function index()
    {
        $messages = Message::select('messages.*', 'users.username as sender_username')
            ->join('users', 'users.id', '=', 'messages.sender')
            ->where('messages.account_id', Auth::id())
            ->orderBy('messages.id', 'desc')
            ->paginate(10);

        $unreadCount = Message::where('account_id', Auth::id())
            ->where('is_read', false)
            ->count();

        return view('inbox.index', compact('messages', 'unreadCount'));
    }

    // Show a single message thread
    public function show($threadId)
    {
        $messages = Message::select(
                'messages.*',
                'sender.username as sender_username',
                'recipient.username as recipient_username'
            )
            ->join('users as sender', 'sender.id', '=', 'messages.sender')
            ->join('users as recipient', 'recipient.id', '=', 'messages.account_id')
            ->where('messages.thread_id', $threadId)
            ->orderBy('messages.created_at', 'asc')
            ->get();

        if ($messages->isEmpty()) {
            abort(404, 'Thread not found');
        }

        // Authorization check: ensure current user is part of the thread
        if (!$messages->where('account_id', Auth::id())->count() &&
            !$messages->where('sender', Auth::id())->count()) {
            abort(403, 'Unauthorized');
        }

        // Mark unread messages as read
        Message::where('thread_id', $threadId)
            ->where('account_id', Auth::id())
            ->where('is_read', false)
            ->update(['is_read' => true]);

        return view('inbox.show', compact('messages'));
    }

    // Reply to a message thread
    public function reply(Request $request, $threadId)
    {
        $originalMessage = Message::where('thread_id', $threadId)->firstOrFail();

        // Use the ReplyService for consistent logic
        $this->replyService->replyTo($originalMessage, $request);

        return redirect()->route('inbox.show', $threadId)->with('success', 'Reply sent!');
    }
}
