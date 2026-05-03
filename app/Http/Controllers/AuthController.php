<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use App\Models\User;

class AuthController extends Controller
{
    public function showLoginForm()
    {
        $recentUsers = User::latest()->take(6)->get();

        return view('login', compact('recentUsers'));
    }

    public function login(Request $request)
    {
        $credentials = $request->only('username', 'password');

        if (Auth::attempt($credentials)) {
            $request->session()->regenerate();
            return redirect()->intended('/home');
        }

        return back()->withErrors(['username' => 'Invalid credentials']);
    }

    public function logout(Request $request)
    {
        Auth::logout();
        $request->session()->invalidate();
        $request->session()->regenerateToken();
        return redirect('/login');
    }

    public function showRegisterForm() {
        return view("register");
    }

    public function register(Request $request) {
        $ip = $request->ip();
        $maxAccountsPerIp = 3;
        $accountsFromIp = \App\Models\User::where('ip_address', $ip)->count();

        if ($accountsFromIp >= $maxAccountsPerIp) {
            return back()->withErrors(['ip' => 'You have reached the maximum number of accounts allowed from this IP.']);
        }

        $request->validate([
            "username" => "required|string|max:28",
            "email" => "required|string|email|max:255|unique:users",
            "password" => "required|string|min:6|max:255|confirmed", // expects password_confirmation field
        ]);

        

        $user = \App\Models\User::create([
            "username" => $request->username,
            "email" => $request->email,
            "password" => bcrypt($request->password),
            'bytes'=>0,
            'bits' => 10,
            "level" => 1,
            "inventory" => '',
            'ip_address' => $ip,
            'tshirtSlot' => null,
            'faceSlot' => null


        ]);

        \Illuminate\Support\Facades\Auth::login($user);
        auth()->user()->sentRequests()->attach(1, ['status'=>'accepted']); // 1 = Yordium account id

        return redirect("/home");
    }

}
