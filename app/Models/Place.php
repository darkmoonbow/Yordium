<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Place extends Model
{
    use HasFactory;

    protected $table = 'places';

    protected $fillable = [
        'title',
        'description',
        'thumbnail',
        'creator_user_id',
        'visits',
        'likes',
        'dislikes',
    ];

    public function creator()
    {
        return $this->belongsTo(User::class, 'creator_user_id');
    }

    public function likesDislikesRatio()
    {
        if ($this->dislikes === 0) {
            return $this->likes;
        }
        return round($this->likes / $this->dislikes, 2);
    }
}
