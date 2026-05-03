<?php

namespace App\Http\Controllers;

use Illuminate\Support\Facades\Auth;
use Illuminate\Http\Request;
use App\Models\Post;
use App\Services\ReplyService;

class ForumController extends Controller
{
    protected $replyService;

    public function __construct(ReplyService $replyService)
    {
        $this->replyService = $replyService;
    }

    // Show a list of top-level posts
    public function index()
    {
        $posts = Post::whereNull('parent_id')
            ->with('replies.user', 'user')
            ->orderBy('created_at', 'desc')
            ->paginate(10);

        return view('forum.index', compact('posts'));
    }

    // Show a single post with its replies
    public function show($postId)
    {
        $post = Post::with(['user', 'replies.user'])
            ->findOrFail($postId);

        if ($post->user_id !== Auth::id() && !$post->replies->contains('user_id', Auth::id())) {
            abort(403, 'Unauthorized');
        }

        $thread = collect([$post])->merge($post->replies);

        return view('forum.show', compact('thread'));
    }

    public function post(Request $request)
    {
        $request->validate([
            'title' => 'required|string|max:127',
            'message' => 'required|string|max:1023'
        ]);

        \App\Models\Post::create([
            'user_id' => Auth::id(),
            'content' => $request->message,
            'parent_id' => null,
            'title' => $request->title
        ]);

        return redirect()->route('forum.index')->with('success', 'Post created!');
    }

    // Store a new top-level post
    public function store(Request $request)
    {
        $request->validate([
            'content' => 'required|string',
        ]);

        Post::create([
            'content' => $request->content,
            'user_id' => Auth::id(),
        ]);

        return redirect()->back()->with('success', 'Post created!');
    }

    // Reply to an existing post
    public function reply(Request $request, $parentId)
    {
        $parentPost = Post::findOrFail($parentId);

        $this->replyService->replyTo($parentPost, $request);

        return redirect()->route('forum.show', $parentId)->with('success', 'Reply posted!');
    }
}
