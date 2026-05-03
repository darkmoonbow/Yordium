<?php

namespace App\Providers;

use Illuminate\Support\ServiceProvider;
use Illuminate\Support\Facades\View;
use Illuminate\Support\Facades\Auth;
use App\Models\Message;

class ViewServiceProvider extends ServiceProvider
{
    public function register(): void
    {
        //
    }

    public function boot(): void
    {
        View::composer('*', function ($view) {
            $unreadCount = 0; 
            $friendRequestCount = 0; // Default for guests

            if (Auth::check()) {
                $user = Auth::user();

                // Count unread messages
                $unreadCount = Message::where('account_id', $user->id)
                    ->where('is_read', false)
                    ->count();

                // Count pending friend requests
                $friendRequestCount = $user->pendingRequests()->count();

                // Share other user data
                $view->with([
                    'bytes' => $user->bytes,
                    'bits' => $user->bits,
                    'unreadCount' => $unreadCount,
                    'friendRequestCount' => $friendRequestCount,
                ]);
            } else {
                $view->with([
                    'unreadCount' => $unreadCount,
                    'friendRequestCount' => $friendRequestCount,
                ]);
            }
        });
    }
}
