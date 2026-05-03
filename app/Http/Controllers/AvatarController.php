<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\User;
use Intervention\Image\ImageManagerStatic as Image;

class AvatarController extends Controller
{
    public function show($userId)
    {
        $user = User::findOrFail($userId);

        $avatar = Image::canvas(256, 256, [0, 0, 0, 0]);

        $colorsMap = [
            0 => [0,0,0],
            1 => [100,100,100],
            2 => [100,0,0],
            3 => [0,100,0],
            4 => [0,0,100],
            5 => [100,100,0],
            6 => [100,100,0],
            7 => [50,0,50],
        ];

        $layers = [
            'legs'  => $colorsMap[$user->legs],
            'arms'  => $colorsMap[$user->arms],
            'torso' => $colorsMap[$user->torso],
            'head'  => $colorsMap[$user->head],
        ];

        foreach ($layers as $layer => $rgb) {
            $path = public_path("images/{$layer}.png");
            if (file_exists($path)) {
                $avatar->insert(Image::make($path)->colorize(...$rgb));
            }
        }

        return $avatar->response('png'); 
        
    }
}
