<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class Post extends Model
{
    protected $table = 'posts';
    protected $fillable = [
        'title',
        'content',
        'user_id',
        'parent_id',
    ];

    public function parent() : BelongsTo
    {
        return $this->belongsTo(Post::class, 'parent_id');
    }

    public function children() : HasMany
    {
        return $this->hasMany(Post::class, 'parent_id');
    }

    public function user() : BelongsTo
    {
        return $this->belongsTo(User::class, 'user_id');
    }

    public function replies() : HasMany
    {
        return $this->hasMany(Post::class, 'parent_id')->with('user');
    }
}
