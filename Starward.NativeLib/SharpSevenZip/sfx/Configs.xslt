<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
	<xsl:template match="/">
		<html>
			<head>
				<title>7-Zip Sfx Configurations</title>
				<style type="text/css">
					h1      { padding: 10px; padding-width: 100% }
					th      { width: 25%; border: 1px solid silver; padding: 10px }
					table   { width: 800px }
					thead   { align: center; background: silver; font-weight: bold }
					td.r1	{ background-color: white }
					td.r2	{ background-color: skyblue }
				</style>
			</head>
			<body>
				<xsl:for-each select="sfxConfigs/config">          
            <table border="1">
              <caption>
                <h1>
                  <xsl:value-of select="@name"/>
                </h1>
              </caption>
              <xsl:if test=".!=''">
                <thead>
                  <th>Command</th>
                  <td align="center">Description</td>
                </thead>
                <tbody>
                  <xsl:for-each select="id">
                    <tr>
                      <xsl:if test="position() mod 2 = 0">
                        <td align="center">
                          <xsl:value-of select="@command"/>
                        </td>
                        <td>
                          <xsl:value-of select="@description"/>
                        </td>
                      </xsl:if>
                      <xsl:if test="position() mod 2 = 1">
                        <td align="center" bgcolor="#d1def0">
                          <xsl:value-of select="@command"/>
                        </td>
                        <td bgcolor="#d1def0">
                          <xsl:value-of select="@description"/>
                        </td>
                      </xsl:if>
                    </tr>
                  </xsl:for-each>
                </tbody>
              </xsl:if>
            </table>          
					<br/>Applicable to modules: <xsl:value-of select="@modules"/>.<br/>
				</xsl:for-each>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>
