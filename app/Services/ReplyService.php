<?php

namespace App\Services;

use Illuminate\Http\Request;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Support\Facades\Auth;

class ReplyService {
    public function ReplyTo(Model $model, Request $request) {
        $request->validate([
            'message' => 'required|string|max:1023'
        ]);

        if($model instanceof \App\Models\Message) {
            return $this->replyToMessage($model, $request);
        }

        if($model instanceof \App\Models\Post) {
            return $this->replyToPost($model, $request);
        }

        throw new \Exception('Unsupported model type'.get_class($model));
    }

    protected function replyToMessage($originalMessage, Request $request) {
        return \App\Models\Message::create([
            'account_id' => $originalMessage->sender === Auth::id()
                ? $originalMessage->account_id
                : $originalMessage->sender,
            'sender' => Auth::id(),
            'message' => $request->message,
            'thread_id' => $originalMessage->thread_d,
            'is_read' => false
        ]);
    }

    protected function replyToPost($originalPost, Request $request) {
        return \App\Models\Post::create([
            'user_id' => Auth::id(),
            'content' => $request->message,
            'parent_id' => $originalPost->parent_id ?? $originalPost->id,
            'title' => $originalPost->title
        ]);
    }
}
