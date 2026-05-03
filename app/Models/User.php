<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;

class User extends Authenticatable
{
    use HasFactory, Notifiable;
    
    protected $fillable = [
        'username',
        'email',       // added
        'password',
        'bits',
        'bytes',
        'level',
        'inventory',   // added
    ];

    protected $hidden = [
        'password',
        'remember_token',
    ];

    protected $casts = [
        'email_verified_at' => 'datetime',
        'password' => 'hashed',
        'inventory' => 'array',  // cast JSON inventory to array automatically
    ];

    public function messages()
    {
        return $this->hasMany(Message::class);
    }

    protected static function booted()
    {
        static::created(function ($user) {
            Message::create([
                'account_id' => $user->id,
                'sender' => 1,
                'message' => 'Welcome to Yordium! You are a beta tester!',
            ]);
        });
    }

        public function receivedRequests() {
        return $this->belongsToMany(User::class, 'friendships', 'friend_id', 'user_id')
            ->withPivot('status')
            ->withTimestamps();
    }

    public function friends()
    {
        return $this->sentRequests()->wherePivot('status', 'accepted')
            ->get()
            ->merge(
                $this->receivedRequests()->wherePivot('status', 'accepted')->get()
        );
    }

    public function pendingRequests()
    {
        return $this->receivedRequests()->wherePivot('status', 'pending');
    }

        public function sentRequests()
    {
        return $this->belongsToMany(User::class, 'friendships', 'user_id', 'friend_id')
                    ->withPivot('status')
                    ->withTimestamps();
                
    }

        public function sentMessages()
    {
        return $this->hasMany(Message::class, 'sender');
    }

    public function receivedMessages()
    {
        return $this->hasMany(Message::class, 'account_id');
    }

    public function isOnline()
    {
        return $this->updated_at->gt(now()->subMinutes(5));
    }



}
