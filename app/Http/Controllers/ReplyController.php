<?php

namespace App\Http\Controllers;

use Illuminate\Support\Facades\Auth;
use Illuminate\Http\Request;
use App\Models\Post;
use App\Services\ReplyService;

class ReplyController extends Controller
{
    public function replyToMessage($threadId, Request $request, ReplyService $replyService)
    {
        $message = Message::where('thread_id', $threadId)
            ->where(function ($query) {
                $query->where('sender', auth()->id())
                      ->orWhere('account_id', auth()->id());
            })
            ->latest()
            ->firstOrFail();

        $replyService->ReplyTo($message, $request);

        return back();
    }

    // New post reply
    public function replyToPost(Post $post, Request $request, ReplyService $replyService)
    {
        $replyService->ReplyTo($post, $request);

        return redirect()->route('forum.show', ['id' => $post->id]);
    }
}
