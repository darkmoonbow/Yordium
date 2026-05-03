<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use App\Models\Item;
class Item extends Model
{
    protected $table = 'items';
    protected $fillable = [
        'name',
        'description',
        'assetid',
        'genre'
    ];


}
