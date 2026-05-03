<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\Item;

class ItemController extends Controller
{
    public function index()
    {
        $items = Item::take(25)->get();
        return view('catalog.index', compact('items'));
    }

    public function show($id)
    {
        $item = Item::findOrFail($id);
        return view('catalog.show', compact('item'));
    }
    public function showByGenre($genre)
    {
        $items = Item::where('genre', $genre)
                     ->orderBy('name')
                     ->get();

        return view('catalog.index', compact('items'));
    }

}
