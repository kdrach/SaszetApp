<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=true displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "form">
        <h1 class="text-2xl font-bold mb-6 text-center text-gray-900 saszet-stagger-1">SaszetApp</h1>
        <#if realm.password>
            <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
                <div class="${properties.kcFormGroupClass!} mb-4 saszet-stagger-2">
                    <label for="username" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1"><#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if></label>

                    <input tabindex="1" id="username" class="${properties.kcInputClass!} saszet-input mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="username" value="${(login.username!'')}"  type="text" autofocus autocomplete="off" />
                    <#if messagesPerField.existsError('username')>
                        <span class="text-sm text-red-600 mt-1 block">${kcSanitize(messagesPerField.get('username'))?no_esc}</span>
                    </#if>
                </div>

                <div class="${properties.kcFormGroupClass!} mb-6 saszet-stagger-3">
                    <label for="password" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1">${msg("password")}</label>

                    <input tabindex="2" id="password" class="${properties.kcInputClass!} saszet-input mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="password" type="password" autocomplete="off" />
                    <#if messagesPerField.existsError('password')>
                        <span class="text-sm text-red-600 mt-1 block">${kcSanitize(messagesPerField.get('password'))?no_esc}</span>
                    </#if>
                </div>

                <div class="${properties.kcFormGroupClass!} ${properties.kcFormSettingClass!} flex items-center justify-between mb-6 saszet-stagger-4">
                    <div id="kc-form-options">
                        <#if realm.rememberMe && !usernameHidden??>
                            <div class="flex items-center">
                                <label class="ml-2 block text-sm text-gray-700 font-medium">
                                    <#if login.rememberMe??>
                                        <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" checked class="h-4 w-4 text-[#10B981] focus:ring-[#10B981] border-gray-300 rounded">
                                    <#else>
                                        <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" class="h-4 w-4 text-[#10B981] focus:ring-[#10B981] border-gray-300 rounded">
                                    </#if>
                                    ${msg("rememberMe")}
                                </label>
                            </div>
                        </#if>
                        </div>
                        <div class="${properties.kcFormOptionsWrapperClass!}">
                            <#if realm.resetPasswordAllowed>
                                <span class="text-sm"><a tabindex="5" href="${url.loginResetCredentialsUrl}" class="font-medium saszet-link text-[#10B981] hover:text-emerald-500">${msg("doForgotPassword")}</a></span>
                            </#if>
                        </div>
                  </div>

                  <div id="kc-form-buttons" class="${properties.kcFormGroupClass!} saszet-stagger-5">
                      <input tabindex="4" class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!} saszet-btn w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-[#10B981] hover:bg-emerald-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#10B981] cursor-pointer" name="login" id="kc-login" type="submit" value="${msg("doLogIn")}"/>
                  </div>
            </form>
        </#if>
        
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration-container" class="mt-6 text-center saszet-stagger-5">
                <div id="kc-registration">
                    <span class="text-sm text-gray-600">${msg("noAccount")} <a tabindex="6" href="${url.registrationUrl}" class="font-semibold saszet-link text-[#10B981] hover:text-emerald-500">${msg("doRegister")}</a></span>
                </div>
            </div>
        </#if>
    </#if>
</@layout.registrationLayout>
