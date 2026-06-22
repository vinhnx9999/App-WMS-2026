import { LoginForm } from "@/components/LoginForm";

export default function LoginPage() {
    return (
        <div className="flex h-screen w-screen items-center justify-center bg-slate-50 dark:bg-slate-950 p-6 md:p-10">
            <div className="w-full max-w-sm md:max-w-3xl">
                <LoginForm />
            </div>
        </div>
    );
}