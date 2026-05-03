<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Message extends Model
{
    protected $table = 'messages';
    protected $fillable = [
        'message',
        'sender',
        'account_id',
        'content',
        'is_read',
        'thread_id',
    ];

    public function thread()
    {
        return $this->hasMany(Message::class, 'thread_id');
    }

    public function parent()
    {
        return $this->belongsTo(Message::class, 'thread_id');
    }


    public $timestamps = true;

    public function senderUser()
    {
        return $this->belongsTo(User::class, 'sender');
    }

    public function recipientUser()
    {
        return $this->belongsTo(User::class, 'account_id');
    }
}
